use std::ops::Sub;

use opencv::{
    core::{
        abs, add, dft, idft, log, magnitude, merge, min_max_loc, no_array, normalize, split,
        subtract, Mat, Point, Rect, Scalar, VecN, BORDER_DEFAULT, CV_32F, CV_32FC1, CV_32S,
        CV_32SC1, CV_64F, CV_64FC1, CV_8UC1, CV_8UC3, CV_8UC4, DFT_COMPLEX_INPUT,
        DFT_COMPLEX_OUTPUT, DFT_REAL_OUTPUT, DFT_SCALE, NORM_MINMAX,
    },
    highgui,
    imgcodecs::{self, IMREAD_COLOR, IMREAD_GRAYSCALE},
    imgproc::{
        circle, cvt_color, dilate, distance_transform, draw_contours, filter_2d, find_contours,
        flood_fill, morphology_default_border_value, sobel, threshold, watershed,
        CHAIN_APPROX_SIMPLE, COLOR_BGR2GRAY, COLOR_GRAY2BGR, COLOR_GRAY2RGB, DIST_L2, INTER_MAX,
        LINE_8, RETR_EXTERNAL, THRESH_BINARY, THRESH_OTSU,
    },
    prelude::*,
    types::VectorOfMat,
    ximgproc::erode,
    Result,
};

fn show(name: &str, mat: &Mat) -> Result<()> {
    highgui::named_window(name, 0)?;
    highgui::imshow(name, &{
        let mut clone = mat.clone();
        mat.convert_to(&mut clone, CV_8UC1, 255.0, 0.0)?;
        clone
    })?;

    Ok(())
}

fn main() -> Result<()> {
    let image_file = {
        let img = imgcodecs::imread("./src.png", IMREAD_GRAYSCALE)?;
        let mut clone = new_mat(CV_32F);
        img.convert_to(&mut clone, CV_32F, 1.0 / 255.0, 0.0)?;
        clone
    };

    show("image file", &image_file)?;

    // let kernel = kernel()?;

    // let mut image_laplacian = new_mat(CV_32F);
    // filter_2d(
    //     &image_file,
    //     &mut image_laplacian,
    //     CV_32F,
    //     &kernel,
    //     Point::new(-1, -1),
    //     0.0,
    //     BORDER_DEFAULT,
    // )?;

    // let image_result = sub_image(&image_file, &image_laplacian)?;

    let image_cvt = convert(&image_file, CV_8UC1, 255.0)?;
    // let image_laplacian = convert_color(&image_laplacian, COLOR_GRAY2RGB)?;

    // highgui::imshow("image_laplacian", &image_laplacian)?;
    // highgui::imshow("image_result", &image_result_cvt)?;

    let mut bw_thr = new_mat(CV_32F);
    threshold(&image_cvt, &mut bw_thr, 100.0, 255.0, THRESH_BINARY)?;

    let peaks = invert(&bw_thr)?; // ***
    highgui::imshow("Peaks", &peaks)?;

    // let mut bw_eroded = new_mat(CV_32F);
    // let kernel1 = Mat::ones(5, 5, CV_8UC1)?;
    // erode(
    //     &bw_thr,
    //     &mut bw_eroded,
    //     &kernel1,
    //     true,
    //     Point::new(0, 0),
    // )?;

    // let mut dist = new_mat(CV_32F);

    // distance_transform(&bw_thr, &mut dist, DIST_L2, 3, CV_32F)?;

    // let mut dist_norm = new_mat(CV_32F);
    // normalize(
    //     &dist,
    //     &mut dist_norm,
    //     0.0,
    //     1.0,
    //     NORM_MINMAX,
    //     -1,
    //     &no_array(),
    // )?;
    // highgui::imshow("Distance Transform Image", &dist_norm)?;

    // let mut dist_norm_thr = new_mat(CV_32F);
    // threshold(&dist_norm, &mut dist_norm_thr, 0.05, 1.0, THRESH_BINARY)?;

    let mut background_markers = new_mat(CV_32F);
    let kernel1 = Mat::ones(5, 5, CV_8UC1)?;
    dilate(
        &peaks,
        &mut background_markers,
        &kernel1,
        Point::new(-1, -1),
        7,
        BORDER_DEFAULT,
        morphology_default_border_value()?,
    )?;

    highgui::imshow("background_markers", &background_markers)?;

    // Searching for contours on peaks Map
    let mut markers = {
        let dist_8u = convert(&peaks, CV_8UC1, 1.0)?;
        let mut contours = VectorOfMat::new();
        find_contours(
            &dist_8u,
            &mut contours,
            RETR_EXTERNAL,
            CHAIN_APPROX_SIMPLE,
            Point::new(0, 0),
        )?;

        let mut markers = Mat::zeros(dist_8u.rows(), dist_8u.cols(), CV_32S)?.to_mat()?;
        println!("contours.len(): {}", contours.len());
        for i in 0..contours.len() {
            draw_contours(
                &mut markers,
                &contours,
                i as i32,
                Scalar::all(i as f64 + 1.0),
                -1,
                LINE_8,
                &no_array(),
                i32::MAX,
                Point::new(0, 0),
            )?;
        }
        for x in 0..dist_8u.rows() {
            for y in 0..dist_8u.cols() {
                if *background_markers.at_2d::<f32>(x, y)? <= 0.1 {
                    *markers.at_2d_mut::<i32>(x, y)? = 255;
                }
            }
            // circle(
            //     &mut markers,
            //     Point::new(x, y),
            //     3,
            //     Scalar::all(255.0),
            //     -1,
            //     LINE_8,
            //     0,
            // )?;
        }
        markers
    };
    let markers_8u = convert(&markers, CV_8UC1, 20.0)?;
    highgui::imshow("Markers", &markers_8u)?;

    let image_result = convert(&convert_color(&image_file, COLOR_GRAY2BGR)?, CV_8UC3, 1.0)?;
    println!("type {}", image_result.typ());

    watershed(&image_result, &mut markers)?;

    let mut mark = image_file.clone();
    for x in 0..mark.rows() {
        for y in 0..mark.cols() {
            if *markers.at_2d::<i32>(x, y)? == -1 {
                *mark.at_2d_mut::<f32>(x, y)? = 255.0;
            }
        }
    }
    highgui::imshow("watershed", &mark)?;

    highgui::wait_key(-1)?;

    Ok(())
}

fn kernel() -> Result<Mat> {
    let mut mat = Mat::zeros(3, 3, CV_64FC1)?.to_mat()?;

    let data = [[1.0, 1.0, 1.0], [1.0, -8.0, 1.0], [1.0, 1.0, 1.0]];

    for i in 0..3 {
        for j in 0..3 {
            *mat.at_2d_mut(i, j)? = data[i as usize][j as usize];
        }
    }

    Ok(mat)
}

fn invert(mat: &Mat) -> Result<Mat> {
    let mut clone = new_mat(CV_32FC1);
    mat.convert_to(&mut clone, CV_32FC1, -1.0, 1.0)?;
    Ok(clone)
}

fn sub_image(left: &Mat, right: &Mat) -> Result<Mat> {
    let mut clone = left.clone();
    subtract(&left, &right, &mut clone, &no_array(), -1)?;
    Ok(clone)
}

fn convert_color(img: &Mat, cvt: i32) -> Result<Mat> {
    let mut clone = img.clone();
    cvt_color(&img, &mut clone, cvt, 0)?;
    Ok(clone)
}

fn convert(mat: &Mat, to: i32, alpha: f64) -> Result<Mat> {
    let mut clone = new_mat(to);
    mat.convert_to(&mut clone, to, alpha, 0.0)?;
    Ok(clone)
}

fn apply<F: Fn(&Mat, &mut Mat) -> Result<()>>(src: &Mat, f: F) -> Result<Mat> {
    let mut clone = src.clone();
    f(src, &mut clone)?;
    Ok(clone)
}

fn new_mat(typ: i32) -> Mat {
    Mat::zeros(0, 0, typ).unwrap().to_mat().unwrap()
}

fn abs_image(image: &Mat) -> Result<Mat> {
    abs(image)?.to_mat()
}

fn correction(image: &Mat) -> Result<Mat> {
    let mut min = 0.0;
    let mut max = 0.0;
    min_max_loc(
        image,
        Some(&mut min),
        Some(&mut max),
        None,
        None,
        &no_array(),
    )?;
    let mut clone = image.clone();
    image.convert_to(&mut clone, CV_32F, 1.0, -min)?;

    let mut clone2 = clone.clone();
    clone.convert_to(&mut clone2, CV_32F, 1.0 / (max - min), 0.0)?;
    Ok(clone2)
}
