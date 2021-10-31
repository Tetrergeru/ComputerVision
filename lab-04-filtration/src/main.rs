use opencv::{
    core::{
        abs, absdiff, add, bitwise_and, bitwise_not, bitwise_or, convert_scale_abs, min_max_loc,
        min_max_loc_sparse, no_array, pow, subtract, Mat, Point, Rect, Scalar, BORDER_CONSTANT,
        BORDER_DEFAULT, CV_16SC1, CV_32F, CV_8SC1, CV_8UC1,
    },
    highgui,
    imgcodecs::{self, IMREAD_GRAYSCALE},
    imgproc::{
        circle, dilate, erode, flood_fill, laplacian, median_blur, morphology_default_border_value,
        sobel, threshold, FILLED, THRESH_TOZERO,
    },
    prelude::*,
    Result,
};

fn show(name: &str, mat: &Mat) -> Result<()> {
    highgui::named_window(name, 0)?;
    highgui::imshow(name, &{
        let mut clone = mat.clone();
        mat.convert_to(&mut clone, CV_8UC1, 1.0, 0.0)?;
        clone
    })?;

    Ok(())
}

fn main() -> Result<()> {
    let image_file = {
        let img = imgcodecs::imread("./skeleton.jpg", IMREAD_GRAYSCALE)?;
        let mut clone = img.clone();
        img.convert_to(&mut clone, CV_32F, 1.0, 0.0)?;
        clone
    };

    show("image_file", &image_file)?;

    let image_laplacian = {
        let mut clone = image_file.clone();
        laplacian(&image_file, &mut clone, CV_32F, 3, 1.0, 0.0, BORDER_DEFAULT)?;
        clone
    };

    let image_laplacian_scaled = &correction(&image_laplacian)?;
    show("image_laplacian", &image_laplacian_scaled)?;

    let image_laplacian_sum = {
        let mut clone = image_laplacian.clone();
        add(
            &image_laplacian,
            &image_file,
            &mut clone,
            &no_array()?,
            CV_32F,
        )?;
        clone
    };

    show("image_laplacian_sum", &image_laplacian_sum)?;

    let image_sobel = {
        let mut clone = image_file.clone();
        let mut clone2 = image_file.clone();
        sobel(
            &image_file,
            &mut clone,
            CV_32F,
            1,
            0,
            3,
            1.0,
            0.0,
            BORDER_DEFAULT,
        )?;
        sobel(
            &image_file,
            &mut clone2,
            CV_32F,
            0,
            1,
            3,
            1.0,
            0.0,
            BORDER_DEFAULT,
        )?;
        let mut clone3 = image_file.clone();
        add(
            &abs_image(&clone)?,
            &abs_image(&clone2)?,
            &mut clone3,
            &no_array()?,
            CV_32F,
        )?;
        correction(&abs_image(&clone3)?)?
    };

    show("image_sobel", &image_sobel)?;

    let image_median_sobel = {
        let mut clone = image_sobel.clone();
        median_blur(&image_sobel, &mut clone, 5)?;
        clone
    };

    show("image_median_sobel", &image_median_sobel)?;

    let image_mask = {
        image_median_sobel
            .mul(&image_laplacian_sum, 1.0 / 256.0)?
            .to_mat()?
    };

    show("image_mask", &image_mask)?;

    let image_mask_sum = {
        let mut clone = image_laplacian.clone();
        add(&image_mask, &image_file, &mut clone, &no_array()?, CV_8UC1)?;
        clone
    };

    show("image_mask_sum", &image_mask_sum)?;

    let img_mat_sum_pow = {
        let mut image_mask_sum_f = image_mask_sum.clone();
        image_mask_sum.convert_to(&mut image_mask_sum_f, CV_32F, 1.0, 0.0)?;

        let mut clone = image_mask_sum.clone();
        pow(&image_mask_sum_f, 0.5, &mut clone)?;

        let mut clone2 = image_mask_sum.clone();
        clone.convert_to(&mut clone2, CV_8UC1, 16.0, 0.0)?;
        clone2
    };

    show("img_mat_sum_pow", &img_mat_sum_pow)?;

    highgui::wait_key(-1)?;

    Ok(())
}

fn abs_image(image: &Mat) -> Result<Mat> {
    abs(image)?.to_mat()
}

fn correction(image: &Mat) -> Result<Mat> {
    let mut min = 0.0;
    let mut max = 0.0;
    let mut min_idx = Point::new(0, 0);
    let mut max_idx = Point::new(0, 0);
    min_max_loc(
        image,
        &mut min,
        &mut max,
        &mut min_idx,
        &mut max_idx,
        &no_array()?,
    )?;
    println!("{}, {}", min, max);
    let mut clone = image.clone();
    image.convert_to(&mut clone, CV_32F, 1.0, -min)?;

    let mut clone2 = clone.clone();
    clone.convert_to(&mut clone2, CV_32F, 256.0 / (max - min), 0.0)?;
    Ok(clone2)
}

fn flooded_image(image: &Mat, seed: (i32, i32), color: (u8, u8, u8)) -> Result<Mat> {
    let mut clone = image.clone();
    let (w, h) = (image.cols(), image.rows());
    flood_fill(
        &mut clone,
        Point::new(seed.0, seed.1),
        Scalar::new(color.0 as f64, color.1 as f64, color.2 as f64, 255.0),
        &mut Rect::new(0, 0, w, h),
        Scalar::new(0.0, 0.0, 0.0, 0.0),
        Scalar::new(255.0, 255.0, 255.0, 255.0),
        4,
    )?;
    Ok(clone)
}

fn not_image(image: &Mat) -> Result<Mat> {
    let mut clone = image.clone();
    bitwise_not(&image, &mut clone, &no_array()?)?;
    Ok(clone)
}

fn or_image(left: &Mat, right: &Mat) -> Result<Mat> {
    let mut clone = left.clone();
    bitwise_or(&left, &right, &mut clone, &no_array()?)?;
    Ok(clone)
}

fn and_image(left: &Mat, right: &Mat) -> Result<Mat> {
    let mut clone = left.clone();
    bitwise_and(&left, &right, &mut clone, &no_array()?)?;
    Ok(clone)
}

fn opening(image: &Mat, size: usize) -> Result<Mat> {
    eroded_image(&dilated_image(image, size, 1)?, size)
}

fn closing(image: &Mat, size: usize) -> Result<Mat> {
    dilated_image(&eroded_image(image, size)?, size, 1)
}

fn dilated_image(image: &Mat, size: usize, times: usize) -> Result<Mat> {
    let mut clone = image.clone();
    let mut str_elem = Mat::zeros(size as i32, size as i32, CV_8UC1)?.to_mat()?;
    circle(
        &mut str_elem,
        Point::new(size as i32 / 2, size as i32 / 2),
        size as i32 / 2,
        Scalar::new(255.0, 255.0, 255.0, 255.0),
        0,
        FILLED,
        0,
    )?;
    let border = morphology_default_border_value()?;
    dilate(
        &image,
        &mut clone,
        &str_elem,
        Point::new(-1, -1),
        times as i32,
        BORDER_CONSTANT,
        border,
    )?;
    Ok(clone)
}

fn eroded_image(image: &Mat, size: usize) -> Result<Mat> {
    let mut clone = image.clone();
    let mut str_elem = Mat::zeros(size as i32, size as i32, CV_8UC1)?.to_mat()?;
    circle(
        &mut str_elem,
        Point::new(size as i32 / 2, size as i32 / 2),
        size as i32 / 2,
        Scalar::new(255.0, 255.0, 255.0, 255.0),
        0,
        FILLED,
        0,
    )?;
    let border = morphology_default_border_value()?;
    erode(
        &image,
        &mut clone,
        &str_elem,
        Point::new(-1, -1),
        1,
        BORDER_CONSTANT,
        border,
    )?;
    Ok(clone)
}

fn diff_image(left: &Mat, right: &Mat) -> Result<Mat> {
    let mut clone = left.clone();
    absdiff(&left, &right, &mut clone)?;
    Ok(clone)
}
