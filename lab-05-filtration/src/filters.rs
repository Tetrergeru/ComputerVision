use opencv::{
    core::{Point, Scalar},
    imgproc::circle,
    prelude::{Mat, MatExprTraitConst, MatTraitConst},
    Result,
};

use crate::{flooded_image, mul_add_image, mul_mat_image};

pub fn apply_filter(fft: &(Mat, Mat), filter: &Mat) -> Result<(Mat, Mat)> {
    Ok((
        mul_mat_image(&fft.0, &filter)?,
        mul_mat_image(&fft.1, &filter)?,
    ))
}

pub fn rev(filter: &Mat) -> Result<Mat> {
    mul_add_image(&mul_add_image(filter, -1.0, 0.0)?, 1.0, 1.0)
}

pub fn perfect_filter(image: &Mat, radius: i32) -> Result<Mat> {
    let mut filter = Mat::zeros(image.rows(), image.cols(), image.typ()?)?.to_mat()?;
    circle(
        &mut filter,
        Point::new(image.rows() / 2, image.cols() / 2),
        radius,
        Scalar::all(1.0),
        0,
        -1,
        0,
    )?;
    let filter = flooded_image(&filter, (image.rows() / 2, image.cols() / 2), (1, 1, 1))?;
    Ok(filter)
}

pub fn butterworth_filter(image: &Mat, radius: i32, n: i32) -> Result<Mat> {
    let filter = Mat::zeros(image.rows(), image.cols(), image.typ()?)?.to_mat()?;
    let center = (image.rows() as f64 / 2.0, image.cols() as f64 / 2.0);
    for i in 0..image.rows() {
        for j in 0..image.cols() {
            let dist = ((i as f64 - center.0).powi(2) + (j as f64 - center.1).powi(2)).sqrt();
            let value = 1.0 / (1.0 + (dist / radius as f64).powi(2 * n));

            unsafe {
                *(filter.at_2d::<f32>(i, j)? as *const f32 as usize as *mut f32) = value as f32;
            }
        }
    }
    Ok(filter)
}

pub fn gaussian_filter(image: &Mat, radius: i32) -> Result<Mat> {
    let filter = Mat::zeros(image.rows(), image.cols(), image.typ()?)?.to_mat()?;
    let center = (image.rows() as f64 / 2.0, image.cols() as f64 / 2.0);
    for i in 0..image.rows() {
        for j in 0..image.cols() {
            let dist = ((i as f64 - center.0).powi(2) + (j as f64 - center.1).powi(2)).sqrt();
            let value = (-dist.powi(2) / (2.0 * (radius as f64).powi(2))).exp();

            unsafe {
                *(filter.at_2d::<f32>(i, j)? as *const f32 as usize as *mut f32) = value as f32;
            }
        }
    }
    Ok(filter)
}
